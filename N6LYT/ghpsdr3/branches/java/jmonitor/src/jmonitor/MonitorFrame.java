/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

/*
 * MonitorFrame.java
 *
 * Created on 31-Dec-2009, 17:31:23
 */

package jmonitor;

/**
 *
 * @author john
 */
public class MonitorFrame extends javax.swing.JFrame {

    /** Creates new form MonitorFrame */
    public MonitorFrame() {
        initComponents();
    }

    /** This method is called from within the constructor to
     * initialize the form.
     * WARNING: Do NOT modify this code. The content of this method is
     * always regenerated by the Form Editor.
     */
    @SuppressWarnings("unchecked")
    // <editor-fold defaultstate="collapsed" desc="Generated Code">//GEN-BEGIN:initComponents
    private void initComponents() {

        monitorPanel1 = new jmonitor.MonitorPanel();

        setDefaultCloseOperation(javax.swing.WindowConstants.EXIT_ON_CLOSE);
        setTitle("JMonitor");
        setResizable(false);

        javax.swing.GroupLayout monitorPanel1Layout = new javax.swing.GroupLayout(monitorPanel1);
        monitorPanel1.setLayout(monitorPanel1Layout);
        monitorPanel1Layout.setHorizontalGroup(
            monitorPanel1Layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGap(0, 480, Short.MAX_VALUE)
        );
        monitorPanel1Layout.setVerticalGroup(
            monitorPanel1Layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGap(0, 100, Short.MAX_VALUE)
        );

        javax.swing.GroupLayout layout = new javax.swing.GroupLayout(getContentPane());
        getContentPane().setLayout(layout);
        layout.setHorizontalGroup(
            layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGroup(javax.swing.GroupLayout.Alignment.TRAILING, layout.createSequentialGroup()
                .addContainerGap(javax.swing.GroupLayout.DEFAULT_SIZE, Short.MAX_VALUE)
                .addComponent(monitorPanel1, javax.swing.GroupLayout.PREFERRED_SIZE, javax.swing.GroupLayout.DEFAULT_SIZE, javax.swing.GroupLayout.PREFERRED_SIZE)
                .addContainerGap())
        );
        layout.setVerticalGroup(
            layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGroup(javax.swing.GroupLayout.Alignment.TRAILING, layout.createSequentialGroup()
                .addContainerGap(javax.swing.GroupLayout.DEFAULT_SIZE, Short.MAX_VALUE)
                .addComponent(monitorPanel1, javax.swing.GroupLayout.PREFERRED_SIZE, javax.swing.GroupLayout.DEFAULT_SIZE, javax.swing.GroupLayout.PREFERRED_SIZE))
        );

        pack();
    }// </editor-fold>//GEN-END:initComponents

    public void updateMonitor(float[] samples) {
        monitorPanel1.updateMonitor(samples);
    }

    // Variables declaration - do not modify//GEN-BEGIN:variables
    private jmonitor.MonitorPanel monitorPanel1;
    // End of variables declaration//GEN-END:variables

}